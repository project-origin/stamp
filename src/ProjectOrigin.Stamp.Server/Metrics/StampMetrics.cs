using System.Diagnostics.Metrics;

namespace ProjectOrigin.Stamp.Server.Metrics;

public interface IStampMetrics
{
    void IncrementIssuedCounter();
    void IncrementIntentsCounter();
}

public class StampMetrics(MeterBase meterBase) : IStampMetrics
{
    private readonly Counter<long> _issuanceIssuedCounter =
        meterBase.Meter.CreateCounter<long>(
            name: "po_stamp_certificate_issued_count",
            unit: "1",
            description: "The number of certificates successfully issued.");

    private readonly Counter<long> _issuanceIntentsCounter =
        meterBase.Meter.CreateCounter<long>(
            name: "po_stamp_certificate_intent_received_count",
            unit: "1",
            description: "The total number of certificate issuance intents received.");

    public void IncrementIssuedCounter() => _issuanceIssuedCounter.Add(1);
    public void IncrementIntentsCounter() => _issuanceIntentsCounter.Add(1);
}
