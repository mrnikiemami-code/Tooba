/**
 * Public boundary for admin known-buyer directory capability.
 */
export { AdminCustomersScreen } from "./components/customers-screen.tsx";
export {
  loadAdminCustomers,
  mapAdminCustomers,
  queryAdminCustomersGrid,
  type AdminCustomerRow,
} from "./api/customers-api.ts";
