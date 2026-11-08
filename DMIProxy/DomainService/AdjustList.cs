using System.Collections.ObjectModel;

namespace DMIProxy.DomainService;

public class AdjustList(Collection<double> values)
{
    private Collection<double> _numbers = [.. values];

    public AdjustList Divide(double fraction)
    {
        _numbers = [.. _numbers.Select(n => n / fraction)];
        return this;
    }

    public AdjustList Multiply(double times)
    {
        _numbers = [.. _numbers.Select(n => n * times)];
        return this;
    }

    public AdjustList Subtract(double subtract)
    {
        _numbers = [.. _numbers.Select(n => n - subtract)];
        return this;
    }

    public AdjustList Round(int digits)
    {
        _numbers = [.. _numbers.Select(n => Math.Round(n, digits))];
        return this;
    }

    public AdjustList Difference()
    {
        var differences = new Collection<double> { 0.0 };
        for (int i = 0; i < _numbers.Count - 1; i++)
        {
            differences.Add(_numbers[i + 1] - _numbers[i]);
        }
        _numbers = differences;
        return this;
    }

    public Collection<double> Run()
    {
        return _numbers;
    }
}
