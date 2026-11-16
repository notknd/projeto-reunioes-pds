CREATE TABLE IF NOT EXISTS rooms (
    id TEXT PRIMARY KEY,
    name TEXT NOT NULL CHECK (length(name) <= 120),
    floor integer NOT NULL,
    capacity integer NOT NULL CHECK (capacity > 0)
);

CREATE TABLE IF NOT EXISTS reservations (
    id TEXT PRIMARY KEY,
    roomid TEXT NOT NULL REFERENCES rooms (id) ON DELETE RESTRICT,
    starttime TEXT NOT NULL,
    endtime TEXT NOT NULL,
    createdat TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_Reservations_RoomId_StartTime_EndTime
    ON reservations (roomid, starttime, endtime);
