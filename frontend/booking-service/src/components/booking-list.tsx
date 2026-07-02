"use client";

import { useEffect, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Button } from "@/components/ui/button";
import { BookingCard } from "@/components/booking-card";
import type { IBookingResponse } from "@/lib/types";

export function BookingList({
  initialBookings,
  initialResourceId,
}: {
  initialBookings: IBookingResponse[];
  initialResourceId: string;
}) {
  const router = useRouter();
  const searchParams = useSearchParams();

  const [resourceId, setResourceId] = useState(initialResourceId);
  const [bookings, setBookings] = useState(initialBookings);

  useEffect(() => {
    setResourceId(initialResourceId);
    setBookings(initialBookings);
  }, [initialBookings, initialResourceId]);

  function handleSearch(e: React.FormEvent) {
    e.preventDefault();

    const trimmed = resourceId.trim();
    const params = new URLSearchParams(searchParams);

    if (trimmed) {
      params.set("resourceId", trimmed);
    } else {
      params.delete("resourceId");
    }

    router.push(`/bookings?${params.toString()}`);
  }

  function handleCancelled(id: string) {
    setBookings((prev) =>
      prev.map((b) =>
        b.id === id ? { ...b, status: "Cancelled" } : b
      )
    );
  }

  return (
    <div className="space-y-6">
      <form onSubmit={handleSearch} className="flex items-end gap-3">
        <div className="flex-1 space-y-2">
          <Label htmlFor="search-resource">Resource</Label>
          <Input
            id="search-resource"
            placeholder="e.g. room-101"
            value={resourceId}
            onChange={(e) => setResourceId(e.target.value)}
          />
        </div>

        <Button type="submit">Search</Button>
      </form>

      {!initialResourceId ? (
        <p className="text-sm text-muted-foreground">
          Enter a resource id above to see its bookings.
        </p>
      ) : bookings.length === 0 ? (
        <p className="text-sm text-muted-foreground">
          No bookings found for &quot;{initialResourceId}&quot;.
        </p>
      ) : (
        <div className="space-y-3">
          {bookings.map((booking) => (
            <BookingCard
              key={booking.id}
              booking={booking}
              onCancelled={handleCancelled}
            />
          ))}
        </div>
      )}
    </div>
  );
}