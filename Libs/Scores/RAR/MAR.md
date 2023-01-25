# MAR Ratio

MAR is a gain-to-pain ratio that is calculated by dividing the Compound Annual Return, CAGR, by the Maximum Drawdown.
For individual strategies, MAR ratio should be at least 0.5. 
Strategies with a MAR ratio above 1.0 are very impressive, but they are a lot harder to come by.
Calmar ratio can be calculated if returns are calculated for the past 36 months. 

```
MAR = CAGR / DD
```

- DD - Maximum drawdown in percents

**Intraday calculation**

When annualized returns are not available, CAGR can be replaced with average returns.

```
MAR = Mean / DD
```

- Mean - Average return
- DD - Maximum drawdown 

**Example** 

S&P 500 buy and hold strategy historically yielded CAGR of 10% with a maximum drawdown of about 55% since 1950.
This gives it a MAR ratio of 0.17.