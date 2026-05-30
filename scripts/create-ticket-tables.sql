CREATE TABLE IF NOT EXISTS "PurchasedTickets" (
    "Id" SERIAL PRIMARY KEY,
    "DrawNumber" INTEGER NOT NULL,
    "DrawDate" TIMESTAMP WITH TIME ZONE,
    "TicketNumber" TEXT,
    "Cost" NUMERIC(10,2) NOT NULL DEFAULT 6.00,
    "Winnings" NUMERIC(10,2) NOT NULL DEFAULT 0,
    "IsChecked" BOOLEAN NOT NULL DEFAULT FALSE,
    "Source" TEXT NOT NULL DEFAULT 'Manual',
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS "PurchasedTicketLines" (
    "Id" SERIAL PRIMARY KEY,
    "TicketId" INTEGER NOT NULL REFERENCES "PurchasedTickets"("Id") ON DELETE CASCADE,
    "LineLabel" TEXT NOT NULL DEFAULT '',
    "Number1" INTEGER NOT NULL,
    "Number2" INTEGER NOT NULL,
    "Number3" INTEGER NOT NULL,
    "Number4" INTEGER NOT NULL,
    "Number5" INTEGER NOT NULL,
    "Number6" INTEGER NOT NULL,
    "Powerball" INTEGER NOT NULL,
    "MainMatches" INTEGER,
    "BonusMatched" BOOLEAN,
    "PowerballMatched" BOOLEAN,
    "Division" TEXT,
    "Prize" NUMERIC(10,2)
);

CREATE INDEX IF NOT EXISTS "IX_PurchasedTickets_DrawNumber" ON "PurchasedTickets" ("DrawNumber");
CREATE INDEX IF NOT EXISTS "IX_PurchasedTickets_CreatedAt" ON "PurchasedTickets" ("CreatedAt");
CREATE INDEX IF NOT EXISTS "IX_PurchasedTicketLines_TicketId" ON "PurchasedTicketLines" ("TicketId");
