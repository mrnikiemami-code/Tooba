# CKEditor font family

- Families: Arial, Tahoma, Verdana, Times New Roman, Georgia, Courier New, B Nazanin (+Tahoma/Arial fallback), Vazirmatn.
- Font Size: tiny/small/default/big/huge + 12px…28px (string options; bare numbers crash FontSize UI).
- English/LTR: `translations` must be `undefined` (not `[]`) — empty array caused `Reduce of empty array with no initial value` and blocked the whole editor once Article locale correctly drove LTR.
- Sanitizer allowlists the configured stacks so Times New Roman / B Nazanin survive Save/reload.
