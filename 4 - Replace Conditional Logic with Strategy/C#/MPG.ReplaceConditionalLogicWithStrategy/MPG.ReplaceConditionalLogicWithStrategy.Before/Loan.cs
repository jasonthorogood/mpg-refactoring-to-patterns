﻿using System;
using System.Collections.Generic;
using System.Linq;

// 1. Create class CapitalStrategy
// 2. Declare Capital in CapitalStrategy
// 3. Copy Capital and anything easy from Loan to CapitalStrategy
// 4. Figure out what is needed from a Loan instance, decide next move, context or data
// 5. Add getter methods for data that is needed from the context
// 6. Make Loan delegate to CapitalStrategy.Capital
// 7. Move YearsTo, WeightedAverageDuration and Duration to CapitalStrategy
// 8. Make Loan delegate to CapitalStrategy.Duration
// 8. Remove unused code from Loan 

namespace MPG.ReplaceConditionalLogicWithStrategy.Before
{
    public class CapitalStrategy
    {
        private const int MillisPerDay = 86400000;
        private const int DaysPerYear = 365;

        public double Capital(Loan loan)
        {
            if (!loan.GetExpiry().HasValue && loan.GetMaturity().HasValue)
            {
                // term loan
                return loan.GetCommitment() * loan.Duration() * RiskFactor(loan);
            }

            if (loan.GetExpiry().HasValue && !loan.GetMaturity().HasValue)
            {
                if (loan.GetUnusedPercentage() != 1.0)
                {
                    // advised line
                    return loan.GetCommitment() * loan.GetUnusedPercentage() * loan.Duration() * RiskFactor(loan);
                }
                else
                {
                    // revolver
                    return (loan.OutstandingRiskAmount() * loan.Duration() * RiskFactor(loan))
                           + (loan.UnusedRiskAmount() * loan.Duration() * UnusedRiskFactor(loan));
                }
            }

            return 0.0;
        }

        private static double RiskFactor(Loan loan)
        {
            return RiskFactors.ForRating(loan.GetRiskRating());
        }

        private static double UnusedRiskFactor(Loan loan)
        {
            return UnusedRiskFactors.ForRating(loan.GetRiskRating());
        }

        public double Duration(Loan loan)
        {
            if (!loan.GetExpiry().HasValue & loan.GetMaturity().HasValue)
            {
                return WeightedAverageDuration(loan);
            }
            else if (loan.GetExpiry().HasValue && !loan.GetMaturity().HasValue)
            {
                return YearsTo(loan.GetExpiry().Value, loan);
            }

            return 0.0;
        }

        private double WeightedAverageDuration(Loan loan)
        {
            var duration = 0.0;
            var weightedAverage = loan.GetPayments().Sum(payment => YearsTo(payment.Date, loan) * payment.Amount);
            var sumOfPayments = loan.GetPayments().Sum(payment => payment.Amount);

            if (loan.GetCommitment() != 0.0)
            {
                duration = weightedAverage / sumOfPayments;
            }

            return duration;
        }

        private double YearsTo(DateTime endDate, Loan loan)
        {
            var beginDate = loan.GetToday().HasValue ? loan.GetToday().Value : loan.GetStart().Value;
            var beginDateMilliseconds = new DateTimeOffset(beginDate).ToUnixTimeMilliseconds();
            var endDateMilliseconds = new DateTimeOffset(endDate).ToUnixTimeMilliseconds();
            return ((endDateMilliseconds - beginDateMilliseconds) / MillisPerDay) / DaysPerYear;
        }
    }

    public class Loan
    {
        private const int MillisPerDay = 86400000;
        private const int DaysPerYear = 365;

        private readonly double _commitment;
        private readonly int _riskRating;
        private readonly double _outstanding;
        private readonly DateTime? _maturity;
        private readonly DateTime? _expiry;
        private readonly List<Payment> _payments = new List<Payment>();
        private readonly DateTime _start;
        private readonly DateTime? _today;
        private double _unusedPercentage;
        private readonly CapitalStrategy _capitalStrategy;

        public Loan(
            double commitment, 
            double outstanding,
            int riskRating,
            DateTime? maturity, 
            DateTime? expiry, 
            DateTime start,
            DateTime? today)
        {
            _commitment = commitment;
            _riskRating = riskRating;
            _outstanding = outstanding;
            _unusedPercentage = 1.0;
            _maturity = maturity;
            _expiry = expiry;
            _start = start;
            _today = today;
            _capitalStrategy = new CapitalStrategy();
        }

        public double GetCommitment()
        {
            return _commitment;
        }

        public DateTime? GetExpiry()
        {
            return _expiry;
        }

        public DateTime? GetMaturity()
        {
            return _maturity;
        }

        public int GetRiskRating()
        {
            return _riskRating;
        }

        public List<Payment> GetPayments()
        {
            return _payments;
        }

        public DateTime? GetToday()
        {
            return _today;
        }

        public DateTime? GetStart()
        {
            return _start;
        }

        internal void SetUnusedPercentage(double unusedPercentage)
        {
            _unusedPercentage = unusedPercentage;
        }

        public double Duration()
        {
            return _capitalStrategy.Duration(this);
        }

        public double Capital()
        {
            return _capitalStrategy.Capital(this);
        }

        public void Payment(double amount, DateTime date)
        {
            _payments.Add(new Payment(amount, date));
        }

        public double OutstandingRiskAmount()
        {
            return _outstanding;
        }

        public double GetUnusedPercentage()
        {
            return _unusedPercentage;
            }

        public double UnusedRiskAmount()
        {
            return _commitment - _outstanding;
        }

        public static Loan NewTermLoan(int commitment, DateTime start, DateTime maturity, int riskRating)
        {
            return new Loan(commitment, commitment, riskRating, maturity, null, start, null);
        }

        public static Loan NewRevolver(double commitment, DateTime start, DateTime expiry, int riskRating)
        {
            return new Loan(commitment, 0, riskRating, null, expiry, start, null);
        }

        public static Loan NewAdvisedLine(double commitment, DateTime start, DateTime expiry, int riskRating)
        {
            if (riskRating > 3) return null;
            var advisedLine = new Loan(commitment, 0, riskRating, null, expiry, start, null);
            advisedLine.SetUnusedPercentage(0.1);
            return advisedLine;
        }
    }
}
