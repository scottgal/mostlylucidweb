import type { SearchOffer, ReservationResult } from './PackingBridge/bin/Release/net10.0/PackingBridge.js';

export interface SalonSession {
    findTogether(from: string, to: string): SearchOffer;
    tryHold(candidateId: string, commandId: string): ReservationResult;
    confirm(reservationId: string, commandId: string): ReservationResult;
    canSarahConsult(at: string): boolean;
    dispose(): void;
}

export function createSalon(databasePath: string): SalonSession;
