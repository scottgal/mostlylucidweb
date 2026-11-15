import type { AutoScheduler, AvailabilityInput, AppSuggestion, DemandView } from './AppScheduler/bin/Release/net10.0/AppScheduler.js';

export interface BookingReply {
    success: boolean;
    reason: string;
    version: number;
    reservationId?: string;
}

export interface CalendarApi {
    readWindow(start: string, end: string): Promise<AvailabilityInput>;
    getName(id: string): string;
    tryBook(candidateId: string, expectedVersion: number, commandId: string, demands: DemandView[]): Promise<BookingReply>;
}

export function createEngine(): AutoScheduler;
export function catalogFor(engine: AutoScheduler): {
    start: string;
    end: string;
    gridMinutes: number;
    resourceIds: string[];
};
export function createSchedulingFlow(api: CalendarApi, engine?: AutoScheduler): {
    suggest(start: string, end: string): Promise<Omit<AppSuggestion, 'demands'>>;
    accept(candidateId: string, commandId: string): Promise<BookingReply>;
};
