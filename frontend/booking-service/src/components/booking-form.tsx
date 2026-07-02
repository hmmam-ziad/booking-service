"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { createBooking, ApiClientError } from "@/lib/api";

export function BookingForm() {
  const router = useRouter();
  const [isSubmitting, setIsSubmitting] = useState(false);

  const [resourceId, setResourceId] = useState("");
  const [userId, setUserId] = useState("");
  const [startDateTime, setStartDateTime] = useState("");
  const [endDateTime, setEndDateTime] = useState("");

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setIsSubmitting(true);

    try {
      // datetime-local input has no timezone info — treat it as UTC explicitly
      const startUtc = new Date(startDateTime + "Z").toISOString();
      const endUtc = new Date(endDateTime + "Z").toISOString();

      await createBooking({
        resourceId: resourceId.trim(),
        userId: userId.trim(),
        startDateTime: startUtc,
        endDateTime: endUtc,
      });

      toast.success("Booking created");
    //   router.push(`/bookings?resourceId=${encodeURIComponent(resourceId.trim())}`);
    } catch (err) {
      if (err instanceof ApiClientError && err.status === 409) {
        toast.error("This resource is already booked for that time window.");
      } else if (err instanceof ApiClientError) {
        toast.error(err.message);
      } else {
        toast.error("Something went wrong. Please try again.");
      }
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <Card className="max-w-lg">
      <CardHeader>
        <CardTitle>New Booking</CardTitle>
      </CardHeader>
      <CardContent>
        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="resourceId">Resource</Label>
            <Input
              id="resourceId"
              placeholder="e.g. room-101"
              value={resourceId}
              onChange={(e) => setResourceId(e.target.value)}
              required
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="userId">Booked by</Label>
            <Input
              id="userId"
              placeholder="e.g. user-1"
              value={userId}
              onChange={(e) => setUserId(e.target.value)}
              required
            />
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="start">Start (UTC)</Label>
              <Input
                id="start"
                type="datetime-local"
                value={startDateTime}
                onChange={(e) => setStartDateTime(e.target.value)}
                required
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="end">End (UTC)</Label>
              <Input
                id="end"
                type="datetime-local"
                value={endDateTime}
                onChange={(e) => setEndDateTime(e.target.value)}
                required
              />
            </div>
          </div>

          <Button type="submit" disabled={isSubmitting} className="w-full">
            {isSubmitting ? "Creating..." : "Create Booking"}
          </Button>
        </form>
      </CardContent>
    </Card>
  );
}