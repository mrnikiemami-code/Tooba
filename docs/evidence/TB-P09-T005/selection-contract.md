# Selection Contract

AdminOrderOperationRequest.Selections: [{ orderLineId, quantity }]

Whole-group: Host derives eligible selections then PackSelectionsAsync / CreateShipmentAsync / UnpackSelectionsAsync — same domain path.

Validation: qty>0, normalize duplicate line ids, line belongs to seller fulfillment, operation-specific remainder checks.
