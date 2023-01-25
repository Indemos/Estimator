# Sortino Ratio and Ulcer Performance Index 

Measures downside volatility of the deviation and can replace standard deviation in the Sharpe ratio formula.

```
Martin = (CAGR - RFR) / UlcerIndex
```

- RFR - Interest Rate
- CAGR - Compound annual growth rate
- UlcerIndex - Standard deviation, but only negative returns are included

**Intraday calculation**

For intraday strategies, RFR is irrelevant. 

```
SR = (N ^ 1/2) * Mean / DSD
```
- Mean - Average return
- DSD - Downside standard deviation in decimal points of percents
