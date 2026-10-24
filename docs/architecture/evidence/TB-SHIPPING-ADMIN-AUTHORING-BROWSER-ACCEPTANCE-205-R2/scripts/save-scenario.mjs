// Architect-prepared FIXED scenario, not a mock backend. All numbers retain source tokens.
import { IDS, editorDocument } from "../../TB-SHIPPING-ADMIN-AUTHORING-BROWSER-ACCEPTANCE-205-R1/scripts/fixtures.mjs";
import { parseJsonNode, writeJsonText, RawNumber } from "../../../../../src/frontend/features/admin-shipping-authoring/api/wire-json.ts";
import { decodeEditorDocument } from "../../../../../src/frontend/features/admin-shipping-authoring/api/editor-document-decode.ts";
export { IDS };
export const NAMES = { fa: "ارسال استاندارد R2", en: "Standard shipping R2" };
export const NEW_TITLE = "گروه آزمایشی";
export const NEW_END = "18:15:00";
function writable(node) {
  if (!node) throw new Error("invalid fixture JSON");
  if (node.kind === "object") return Object.fromEntries([...node.fields].map(([k,v])=>[k,writable(v)]));
  if (node.kind === "array") return node.items.map(writable);
  if (node.kind === "number") return new RawNumber(node.raw);
  return node.kind === "null" ? null : node.value;
}
function base() { return writable(parseJsonNode(editorDocument)); }
function newRow(id) {
  return {rateRowId:id,minWeightGrams:null,maxWeightGrams:null,minItemCount:null,maxItemCount:null,
    minMerchandiseAmount:null,maxMerchandiseAmount:null,freightAmount:new RawNumber("0.50"),
    packagingAmount:new RawNumber("0.00"),minDeliveryDays:1,maxDeliveryDays:2};
}
const newArea = {kind:"Country",countryId:IDS.country2,administrativeAreaId:null,cityId:null,serviceAreaId:null};
const newFact = {kind:"Country",id:IDS.country2,resolution:"Resolved",area:newArea,label:"آلمان"};
export const initialResponseText = editorDocument;
const saved=base();
saved.revision=8;saved.type.revision=8;saved.updatedAt="2026-10-24T16:30:00Z";
saved.presentation.methodTranslations=saved.presentation.methodTranslations.map(t=>({...t,name:t.languageId===IDS.fa?NAMES.fa:NAMES.en}));
saved.weeklySchedule.days[0].endTime=NEW_END;
saved.regionGroups.push({groupId:IDS.newGroup,order:2,currencyCode:"IRR",
  areas:[{kind:"Country",id:IDS.country2}],rows:[newRow(IDS.newRow)],titles:[{languageId:IDS.fa,title:NEW_TITLE}]});
saved.destinationFacts.push(newFact);
export const savedResponseText=writeJsonText(saved);
const initial=base();
const knownFacts=new Map(initial.destinationFacts.map(f=>[f.kind+":"+f.id,f.area]));
export const expectedRequestText=writeJsonText({
  expectedRevision:7,
  regionGroups:[
    ...initial.regionGroups.map(g=>({groupId:g.groupId,areas:g.areas.map(a=>knownFacts.get(a.kind+":"+a.id)),rows:g.rows,titles:[]})),
    {groupId:null,areas:[newArea],rows:[newRow(null)],titles:[{languageId:IDS.fa,title:NEW_TITLE}]}
  ],
  methodTranslations:initial.presentation.methodTranslations.map(t=>({...t,name:t.languageId===IDS.fa?NAMES.fa:NAMES.en})),
  weeklySchedule:{...initial.weeklySchedule,days:initial.weeklySchedule.days.map((d,i)=>i===0?{...d,endTime:NEW_END}:d)}
});
function canonical(value) {
  if(Array.isArray(value))return value.map(canonical);
  if(value&&typeof value==="object")return Object.fromEntries(Object.keys(value).sort().map(k=>[k,canonical(value[k])]));
  return value;
}
export function assertScenarioRequest(text) {
  if(JSON.stringify(canonical(JSON.parse(text)))!==JSON.stringify(canonical(JSON.parse(expectedRequestText))))
    throw new Error("scenario request differs: do NOT return success; inspect captured body");
  for(const token of ['"freightAmount":0.50','"packagingAmount":1850000.00','"startTime":"09:00:00.500"'])
    if(!text.includes(token))throw new Error("lost exact token "+token);
}
export function preflight() {
  for(const [label,text] of [["initial",initialResponseText],["saved",savedResponseText]]) {
    const decoded=decodeEditorDocument(parseJsonNode(text),IDS.draft);
    if(!decoded.ok)throw new Error(label+":"+decoded.reason);
    if(decoded.value.draftId!==IDS.draft)throw new Error("wrong draft");
  }
  assertScenarioRequest(expectedRequestText);
  const negative=JSON.parse(savedResponseText);
  negative.regionGroups[0].areas=JSON.parse(expectedRequestText).regionGroups[0].areas;
  if(decodeEditorDocument(parseJsonNode(JSON.stringify(negative)),IDS.draft).ok)
    throw new Error("negative: write areas accepted as read areas");
  return {initialRevision:7,savedRevision:8,newGroupId:IDS.newGroup,newRowId:IDS.newRow,validReadResponses:2,invalidWriteAsReadRejected:true};
}
