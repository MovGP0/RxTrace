namespace RxTrace;

public record RxEventRecord(string Source, string Target, string Payload, DateTimeOffset Timestamp);