# HouseKeeper.AppHost

Development-only .NET Aspire composition root for the local HouseKeeper topology.

It currently orchestrates:

- `HouseKeeper.Api`;
- PostgreSQL 18.4 and the `housekeeper` database;
- Azurite and the `attachments` blob service.

The AppHost is not a production deployable and must not contain business rules, migration logic or provider credentials.
