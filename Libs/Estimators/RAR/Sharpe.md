# Sharpe Ratio 

Sharpe ratio is a measure of risk–adjusted performance that indicates the level of excess return per unit of risk.
Any strategy with ratio less than 1, after including execution costs, is usually ignored. 
Most quantitative hedge funds ignore strategies with annualized Sharpe ratio less than 2.
For normal distribution, over 99% of random values are within the range of ±3σ about the mean value. 
It follows that the value of Sharpe Ratio exceeding 3 is very good.

- For a retail algorithmic trader, an annualized Sharpe ratio greater than 2 is pretty good.
- For HFT, the ratio can go up in double digits, especially for opportunity-driven but not highly scalable strategies.

```
SR = (AHPR - (1 + RFR)) / SD
```

For intraday strategies RFR can be skipped. 

```
SR = (N ^ 1/2) * Mean / SD
```

- AHPR - Average holding period returns
- RFR - Risk free rate
- Mean - Average return
- SD - Standard deviation
- N - Number of trades
