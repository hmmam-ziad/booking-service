export type BookingStatus = "Confirmed" | "Cancelled";

export interface IBookingResponse {
  id: string;
  resourceId: string;
  userId: string;
  startDateTime: string;
  endDateTime: string;
  status: BookingStatus;
  createdAt: string;
  cancelledAt: string | null;
}

export interface ICreateBookingRequest {
  resourceId: string;
  userId: string;
  startDateTime: string;
  endDateTime: string;
}

export interface IPagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface IApiError {
  error: string;
}