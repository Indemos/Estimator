# Sterling Ratio 

Measures downside volatility using average loss and can replace standard deviation in the Sharpe ratio formula.

```
SR = (AHPR - (1 + RFR)) / MeanLoss
```

For intraday strategies RFR can be skipped. 

```
SR = (N ^ 1/2) * Mean / MeanLoss
```

- Mean - Average return
- MeanLoss - Average loss
- AHPR - Average holding period returns
- RFR - Interest Rate
