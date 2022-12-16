# Kestner Ratio 

K-ratio measures risk versus return by analyzing how steady a portfolio's returns are over time. 
The K-ratio takes into account the returns, but also the order of those returns in measuring risk.
A higher k-ratio indicates a higher positive consistency in trading performance. 
A ratio higher than 2.0 or so is generally considered good.

```
Slope = Cov(X,Y) / Cov(X,X)
```

- Cov(X,Y) - Covariance between series
- Cov(X,X) - Variance of the regression line

```
Error = (Deviation / N) ^ 1/2
```

- Deviation - Standard deviation
- N - Number of trades

```
KR = Slope / Error
```
