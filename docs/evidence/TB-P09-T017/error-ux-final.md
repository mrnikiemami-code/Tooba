# Error UX final — TB-P09-T017

Touched cancel/blocked-action codes map to FA via `admin-error-map.ts`. Host 400s use `errorCode`. No Bad Request / stack in Order Detail source. Runtime dispatched cancel returns human FA, not raw JSON as the user-facing title.
