export interface FDIdentification {
  fdId: number;
  fdReferenceNo: string;
  entityId: number;
  counterpartyId: number;
  counterpartyType?: string;
  currencyCode: string;
  principalAmount: number;
  startDate: string;
  endDate: string;
  settlementDate: string;
  bankAccountId?: number;
}
