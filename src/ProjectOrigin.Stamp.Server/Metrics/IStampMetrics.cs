namespace ProjectOrigin.Stamp.Server.Metrics;

public interface IStampMetrics
{
    void UpdateGauges();

    void AddCertificatesIssued(long count);
}
