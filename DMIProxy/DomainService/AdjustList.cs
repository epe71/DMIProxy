namespace DMIProxy.DomainService
{
    public class AdjustList
    {
        private List<double> _numbers;

        public AdjustList(List<double> values) 
        { 
            _numbers = values;
        }

        public AdjustList Divide(double fraction)
        {
            _numbers = _numbers.Select(n => n / fraction).ToList();
            return this;
        }

        public AdjustList Multiply(double times)
        {
            _numbers = _numbers.Select(n => n * times).ToList();
            return this;
        }

        public AdjustList Subtract(double subtract)
        {
            _numbers = _numbers.Select(n => n - subtract).ToList();
            return this;
        }

        public AdjustList Round(int digits)
        {
            _numbers = _numbers.Select(n => Math.Round(n, digits)).ToList();
            return this;
        }

        public AdjustList Difference()
        {
            var differences = new List<double> { 0.0 };
            for (int i = 0; i < _numbers.Count - 1; i++)
            {
                differences.Add(_numbers[i + 1] - _numbers[i]);
            }
            _numbers = differences;
            return this;
        }

        public List<double> Run()
        {
            return _numbers;
        }
    }
}
