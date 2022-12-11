# Sharpe Ratio 

Commonly used formula for annualized returns. 
Any strategy with ratio less than 1, after including execution costs, is usually ignored. 
Most quantitative hedge funds ignore strategies with annualized Sharpe ratio less than 2.

- For a retail algorithmic trader, an annualized Sharpe ratio greater than 2 is pretty good.
- For high-frequency trading, as discussed, the ratio can go up in double digits as well, especially for opportunity-driven but not highly scalable strategies.

**Annualized ratio**

```
SR = (AHPR - (1 + RFR)) / SD
```

- SR - Sharpe ratio
- AHPR - Average holding period returns
- RFR - Risk free rate in decimal points of percents
- SD - Standard deviation in indecimal points of percents

** Alternative calculaltion** 

```
SR = (Mean - RFR) / SD
```

- Mean - Average returns in decimal points of percents

**Intraday calculation** 

The risk-free rate can be considered to be 0 since we don't roll over positions, there is no interest charge.

```
SR = (N ^ 2) * Mean / SD
```

- N - number of trades