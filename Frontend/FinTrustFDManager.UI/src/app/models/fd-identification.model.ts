export interface FDIdentification {
  fdId: number;
  fdReferenceNo: string;
  entityId: number;
  counterpartyId: number;
  counterpartyType?: string;
  currencyId: number;
  principalAmount: number;
  startDate: string;
  endDate: string;
  settlementDate: string;
}
