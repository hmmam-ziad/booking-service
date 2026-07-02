import { BookingForm } from "@/components/booking-form";

const page = () => {
    return(
        <div>
            <h1 className="mb-6 text-2xl font-semibold">Create a Booking</h1>
            <BookingForm />
        </div>
    );
}

export default page