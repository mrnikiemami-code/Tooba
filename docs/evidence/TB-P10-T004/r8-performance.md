Orders/Payments grid: one GetStatusesAsync per page (2 fulfillment queries + inventory evals server-side). FE: zero supply-status calls from list rows. Detail: one supply-status. No polling.
