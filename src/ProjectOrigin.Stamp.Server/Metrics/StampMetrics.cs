using System.Diagnostics.Metrics;

namespace ProjectOrigin.Stamp.Server.Metrics;

public class StampMetrics : IStampMetrics
{
    public const string MetricName = "StampMetrics";

    private long _certificatesIssued;
    private long _certificatesIssuedCounter;
    private ObservableCounter<long> _certificatesIssuedCounterObs;
    private long _certificatesIssuedGauge;
    private ObservableGauge<long> _certificatesIssuedGaugeObs;

    public long CurrentCertificatesIssuedCounter => _certificatesIssuedCounter;


    public StampMetrics()
    {
        var meter = new Meter(MetricName);

        _certificatesIssuedCounterObs = meter.CreateObservableCounter(
            name: "certificates_issued",
            observeValue: () => _certificatesIssuedCounter,
            description: "Total number of certificates that have been issued."
        );

        _certificatesIssuedGaugeObs = meter.CreateObservableGauge(
            name: "certificates_issued_gauge",
            observeValue: () => _certificatesIssuedGauge,
            description: "Delta gauge for newly issued certificates since last update."
        );
    }

    public void UpdateGauges()
    {
        _certificatesIssuedGauge = _certificatesIssuedCounter - _certificatesIssued;

        _certificatesIssued = _certificatesIssuedCounter;
    }

    public void AddCertificatesIssued(long count)
    {
        _certificatesIssuedCounter += count;
    }

    public void Reset()
    {
        _certificatesIssued = 0;
        _certificatesIssuedCounter = 0;
        _certificatesIssuedGauge = 0;
    }
}
