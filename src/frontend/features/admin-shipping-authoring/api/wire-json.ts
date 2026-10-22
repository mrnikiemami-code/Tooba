/**
 * Strict JSON wire primitives for the shipping authoring editor adapter.
 *
 * The admin editor document carries `decimal` money amounts and `TimeOnly` values. Decoding them
 * through `JSON.parse` would silently reformat `0.50` to `0.5` and re-encode times, so this module
 * keeps the exact number/time text (`raw`) and refuses structurally ambiguous payloads such as
 * duplicate object keys. It is a read/write primitive only: no business rules, no fallbacks.
 */

/** A parsed JSON node that preserves the exact source text of numbers. */
export type JsonNode =
  | { readonly kind: "object"; readonly fields: ReadonlyMap<string, JsonNode> }
  | { readonly kind: "array"; readonly items: readonly JsonNode[] }
  | { readonly kind: "string"; readonly value: string }
  | { readonly kind: "number"; readonly raw: string }
  | { readonly kind: "boolean"; readonly value: boolean }
  | { readonly kind: "null" };

/** Text that must be written as an exact JSON number token (never quoted, never reformatted). */
export class RawNumber {
  readonly raw: string;

  constructor(raw: string) {
    this.raw = raw;
  }
}

/** Value accepted by {@link writeJsonText}. */
export type JsonWrite =
  | string
  | number
  | boolean
  | null
  | RawNumber
  | readonly JsonWrite[]
  | { readonly [key: string]: JsonWrite };

class ParseFailure extends Error {}

const NUMBER = /^-?(?:0|[1-9][0-9]*)(?:\.[0-9]+)?(?:[eE][+-]?[0-9]+)?$/;
const DECIMAL = /^(?:0|[1-9][0-9]*)(?:\.[0-9]+)?$/;

/**
 * Parses JSON text into {@link JsonNode}, preserving exact number text and rejecting duplicate keys.
 *
 * @param text Raw response/request text.
 * @returns The parsed node, or `null` when the text is not valid JSON (never a partial value).
 */
export function parseJsonNode(text: string): JsonNode | null {
  const parser = new Reader(text);
  try {
    const node = parser.value();
    parser.skipWhitespace();
    if (parser.index !== text.length) return null;
    return node;
  } catch (error) {
    if (error instanceof ParseFailure) return null;
    throw error;
  }
}

/**
 * Serializes a value to compact JSON text, emitting {@link RawNumber} entries as exact number tokens.
 *
 * @param value Value to serialize.
 * @returns Compact JSON text with declaration order preserved.
 */
export function writeJsonText(value: JsonWrite): string {
  const out: string[] = [];
  emit(value, out);
  return out.join("");
}

/**
 * Validates a non-negative decimal amount text as used by the contract (`decimal`, no exponent).
 *
 * @param raw Candidate number token.
 * @returns True when the token is a plain decimal number.
 */
export function isDecimalText(raw: string): boolean {
  return DECIMAL.test(raw);
}

/**
 * Validates an integer number token.
 *
 * @param raw Candidate number token.
 * @returns True when the token is a plain integer.
 */
export function isIntegerText(raw: string): boolean {
  return /^-?(?:0|[1-9][0-9]*)$/.test(raw);
}

class Reader {
  readonly text: string;
  index = 0;

  constructor(text: string) {
    this.text = text;
  }

  skipWhitespace(): void {
    while (this.index < this.text.length) {
      const code = this.text.charCodeAt(this.index);
      if (code === 0x20 || code === 0x09 || code === 0x0a || code === 0x0d) this.index += 1;
      else return;
    }
  }

  value(): JsonNode {
    this.skipWhitespace();
    const char = this.text[this.index];
    if (char === "{") return this.object();
    if (char === "[") return this.array();
    if (char === '"') return { kind: "string", value: this.string() };
    if (this.text.startsWith("true", this.index)) {
      this.index += 4;
      return { kind: "boolean", value: true };
    }
    if (this.text.startsWith("false", this.index)) {
      this.index += 5;
      return { kind: "boolean", value: false };
    }
    if (this.text.startsWith("null", this.index)) {
      this.index += 4;
      return { kind: "null" };
    }
    return this.number();
  }

  object(): JsonNode {
    this.index += 1;
    const fields = new Map<string, JsonNode>();
    this.skipWhitespace();
    if (this.text[this.index] === "}") {
      this.index += 1;
      return { kind: "object", fields };
    }
    for (;;) {
      this.skipWhitespace();
      if (this.text[this.index] !== '"') throw new ParseFailure("object-key");
      const key = this.string();
      if (fields.has(key)) throw new ParseFailure("duplicate-key");
      this.skipWhitespace();
      if (this.text[this.index] !== ":") throw new ParseFailure("object-colon");
      this.index += 1;
      fields.set(key, this.value());
      this.skipWhitespace();
      const next = this.text[this.index];
      if (next === ",") {
        this.index += 1;
        continue;
      }
      if (next === "}") {
        this.index += 1;
        return { kind: "object", fields };
      }
      throw new ParseFailure("object-separator");
    }
  }

  array(): JsonNode {
    this.index += 1;
    const items: JsonNode[] = [];
    this.skipWhitespace();
    if (this.text[this.index] === "]") {
      this.index += 1;
      return { kind: "array", items };
    }
    for (;;) {
      items.push(this.value());
      this.skipWhitespace();
      const next = this.text[this.index];
      if (next === ",") {
        this.index += 1;
        continue;
      }
      if (next === "]") {
        this.index += 1;
        return { kind: "array", items };
      }
      throw new ParseFailure("array-separator");
    }
  }

  string(): string {
    this.index += 1;
    let value = "";
    for (;;) {
      if (this.index >= this.text.length) throw new ParseFailure("string-eof");
      const char = this.text[this.index];
      if (char === '"') {
        this.index += 1;
        return value;
      }
      if (char === "\\") {
        value += this.escape();
        continue;
      }
      if (char !== undefined && char.charCodeAt(0) < 0x20) throw new ParseFailure("string-control");
      value += char;
      this.index += 1;
    }
  }

  escape(): string {
    const marker = this.text[this.index + 1];
    this.index += 2;
    switch (marker) {
      case '"':
        return '"';
      case "\\":
        return "\\";
      case "/":
        return "/";
      case "b":
        return "\b";
      case "f":
        return "\f";
      case "n":
        return "\n";
      case "r":
        return "\r";
      case "t":
        return "\t";
      case "u": {
        const hex = this.text.slice(this.index, this.index + 4);
        if (!/^[0-9a-fA-F]{4}$/.test(hex)) throw new ParseFailure("escape-unicode");
        this.index += 4;
        return String.fromCharCode(Number.parseInt(hex, 16));
      }
      default:
        throw new ParseFailure("escape");
    }
  }

  number(): JsonNode {
    const start = this.index;
    while (this.index < this.text.length) {
      const char = this.text[this.index];
      if (char !== undefined && /[-+0-9.eE]/.test(char)) this.index += 1;
      else break;
    }
    const raw = this.text.slice(start, this.index);
    if (!NUMBER.test(raw)) throw new ParseFailure("number");
    return { kind: "number", raw };
  }
}

function emit(value: JsonWrite, out: string[]): void {
  if (value === null) {
    out.push("null");
    return;
  }
  if (value instanceof RawNumber) {
    if (!NUMBER.test(value.raw)) throw new Error(`raw_number_invalid:${value.raw}`);
    out.push(value.raw);
    return;
  }
  if (typeof value === "string") {
    out.push(JSON.stringify(value));
    return;
  }
  if (typeof value === "boolean") {
    out.push(value ? "true" : "false");
    return;
  }
  if (typeof value === "number") {
    if (!Number.isFinite(value)) throw new Error(`number_invalid:${String(value)}`);
    out.push(JSON.stringify(value));
    return;
  }
  if (Array.isArray(value)) {
    const items = value as readonly JsonWrite[];
    out.push("[");
    items.forEach((item, index) => {
      if (index > 0) out.push(",");
      emit(item, out);
    });
    out.push("]");
    return;
  }
  const node = value as { readonly [key: string]: JsonWrite };
  out.push("{");
  let first = true;
  for (const key of Object.keys(node)) {
    const entry = node[key];
    if (entry === undefined) continue;
    if (!first) out.push(",");
    first = false;
    out.push(JSON.stringify(key), ":");
    emit(entry, out);
  }
  out.push("}");
}
