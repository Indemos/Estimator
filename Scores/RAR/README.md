# Risk Adjusted Return 

Risk adjusted return is a ratio between potential profit and risk compared to a virtually risk-free investment.
In other words, it checks if the current strategy is more profitable then risk free investment, like treasuries.
For intraday strategies, the risk-free rate can be considered to be 0 since we don't roll over positions, there is no interest charge.

```
RAR = (N ^ 2) * Mean / Risk
```

- Mean - Average return
- Risk - Measure of risk, e.g. standard deviation
- N - Number of trades

**Ratios**

Adjusting denominator, the formula can produce different ratios.

- MAR - Risk is defined as max loss in a series
- Sharpe - Risk is defined as standard deviation 
- Sortino - Risk is defined as downside standard deviation 
- Sterling - Risk is defined as average loss in a series
