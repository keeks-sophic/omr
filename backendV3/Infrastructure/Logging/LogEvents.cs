namespace BackendV3.Infrastructure.Logging;

public static class LogEvents
{
    public static class Api
    {
        public const string RequestStart = "api.request.start";
        public const string RequestEnd = "api.request.end";
        public const string RequestException = "api.request.exception";
    }

    public static class Db
    {
        public const string MigrationStart = "db.migration.start";
        public const string MigrationEnd = "db.migration.end";
        public const string MigrationFailed = "db.migration.failed";
        public const string TransactionBegin = "db.transaction.begin";
        public const string TransactionCommit = "db.transaction.commit";
        public const string TransactionRollback = "db.transaction.rollback";
        public const string Retry = "db.retry";
        public const string ConstraintViolation = "db.constraintViolation";
    }

    public static class SignalR
    {
        public const string Connect = "signalr.connect";
        public const string Disconnect = "signalr.disconnect";
        public const string InvocationStart = "signalr.invocation.start";
        public const string InvocationEnd = "signalr.invocation.end";
        public const string InvocationFailed = "signalr.invocation.failed";
        public const string EventPublish = "signalr.event.publish";
    }

    public static class Nats
    {
        public const string Connect = "nats.connect";
        public const string Disconnect = "nats.disconnect";
        public const string Publish = "nats.publish";
        public const string ConsumeStart = "nats.consume.start";
        public const string ConsumeEnd = "nats.consume.end";
        public const string MessageAck = "nats.message.ack";
        public const string MessageNak = "nats.message.nak";
        public const string MessageFailed = "nats.message.failed";
    }
}

