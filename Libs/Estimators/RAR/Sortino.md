# Sortino Ratio and Ulcer Performance Index 

Measures downside volatility of the deviation and can replace standard deviation in the Sharpe ratio formula.

```
Martin = (CAGR - RFR) / UlcerIndex
```

For intraday strategies RFR can be skipped. 

```
SR = (N ^ 1/2) * Mean / DSD
```

- Mean - Average return
- DSD - Downside standard deviation
- RFR - Interest Rate
- CAGR - Compound annual growth rate
- UlcerIndex - Standard deviation, but only negative returns are included
