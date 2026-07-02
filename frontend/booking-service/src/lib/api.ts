import { IBookingResponse, ICreateBookingRequest, IPagedResult } from "./types";

// dev-only
if(process.env.NODE_ENV === "development")
            process.env.NODE_TLS_REJECT_UNAUTHORIZED = "0";
const APIUrl = process.env.NEXT_PUBLIC_API_URL;

// Custom error class for API client errors.
class ApiClientError extends Error { 
    constructor(message: string, public status: number) {
        super(message);
        this.name = "ApiClientError";
    }
}

// Handle API errors and parse the response body.
async function handleResponse<T>(response: Response): Promise<T> {
    if(!response.ok) {
        let message = `Request failed with status ${response.status}`;
        try {
            const body = await response.json();
            if(body?.error) message = body.error;
        }catch {}
        throw new ApiClientError(message, response.status);
    }
    if(response.status === 204) return undefined as T; // No content
    return response.json();
}

// Create a new booking.
export async function createBooking(payload: ICreateBookingRequest): Promise<IBookingResponse> {
    const res = await fetch(`${APIUrl}/Bookings`, {
        method: "POST",
        headers: {
            "Content-Type": "application/json"
        },
        body: JSON.stringify(payload)
    });

    return handleResponse<IBookingResponse>(res);
}

// Retrieve bookings for a specific resource.
export async function getBookingsByResource(resourceId: string, opt?: {
    from?: string;
    to?: string;
    page?: number;
    pageSize?: number;
}): Promise<IPagedResult<IBookingResponse>> {
    const params = new URLSearchParams({resourceId});
    if(opt?.from) params.set("from", opt.from);
    if(opt?.to) params.set("to", opt.to);
    if(opt?.page) params.set("page", opt.page.toString());
    if(opt?.pageSize) params.set("pageSize", opt.pageSize.toString());

    const res = await fetch(`${APIUrl}/Bookings?${params.toString()}`, {
        cache: "no-store"
    });

    return handleResponse<IPagedResult<IBookingResponse>>(res);
}

// Cancel an existing booking.
export async function cancelBooking(id: string): Promise<void> {
    const res = await fetch(`${APIUrl}/Bookings/${id}`, {
        method: "DELETE"
    });
    return handleResponse<void>(res);
}


export {ApiClientError};