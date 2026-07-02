"use client";

import { useState } from "react";
import { toast } from "sonner";
import { Card, CardContent } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { cancelBooking, ApiClientError } from "@/lib/api";
import type { IBookingResponse } from "@/lib/types";

function formatDateTime(iso: string) {
  return new Date(iso).toLocaleString("en-GB", {
    dateStyle: "medium",
    timeStyle: "short",
    timeZone: "UTC",
  }) + " UTC";
}

export function BookingCard({
  booking,
  onCancelled,
}: {
  booking: IBookingResponse;
  onCancelled: (id: string) => void;
}) {
  const [isCancelling, setIsCancelling] = useState(false);
  const isActive = booking.status === "Confirmed";

  async function handleCancel() {
    setIsCancelling(true);
    try {
      await cancelBooking(booking.id);
      toast.success("Booking cancelled");
      onCancelled(booking.id);
    } catch (err) {
      if (err instanceof ApiClientError) {
        toast.error(err.message);
      } else {
        toast.error("Could not cancel booking. Please try again.");
      }
    } finally {
      setIsCancelling(false);
    }
  }

  return (
    <Card>
      <CardContent className="flex items-center justify-between gap-4 py-4">
        <div className="space-y-1">
          <div className="flex items-center gap-2">
            <span className="font-medium">{booking.resourceId}</span>
            <Badge variant={isActive ? "default" : "secondary"}>
              {booking.status}
            </Badge>
          </div>
          <p className="text-sm text-muted-foreground">
            {formatDateTime(booking.startDateTime)} → {formatDateTime(booking.endDateTime)}
          </p>
          <p className="text-xs text-muted-foreground">Booked by {booking.userId}</p>
        </div>

        {isActive && (
          <Button
            variant="outline"
            size="sm"
            onClick={handleCancel}
            disabled={isCancelling}
          >
            {isCancelling ? "Cancelling..." : "Cancel"}
          </Button>
        )}
      </CardContent>
    </Card>
  );
}