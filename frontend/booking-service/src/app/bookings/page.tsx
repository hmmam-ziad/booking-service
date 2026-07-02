import { BookingList } from "@/components/booking-list";
import { getBookingsByResource } from "@/lib/api";

const page = async ({
  searchParams,
}: {
  searchParams: Promise<{ resourceId?: string }>;
}) => {
    const { resourceId } = await searchParams;
    const trimmedResourceId = resourceId?.trim() ?? "";

    const initialBookings = trimmedResourceId
        ? (await getBookingsByResource(trimmedResourceId)).items
        : [];

    return(
        <div>
            <h1 className="mb-6 text-2xl font-semibold">Bookings</h1>
            <BookingList
                initialBookings={initialBookings}
                initialResourceId={trimmedResourceId}
            />
        </div>
    );
}

export default page