# Linear Regression Correlation 

Correlation between a series and its linear regression line that most precisely describes strategy's returns.
Regression is drawn as a straight line from the first to the last value in the series. 
This allows to estimate whether gains or losses stay consistent over time and don't deviate too much from expected value. 
Value varies from -1 for negative to +1 for positive correlation, which means that trading is pretty close to predefined regresion line and expectation. 
The value of 0 means that there is no correlation because the trading style is changing or results are inconsistent over time. 

```
Corr = Cov(X,Y) / (Sx x Sy)
```

- Corr - Correlation between original series of returns and its regression line 
- Cov - Covariance between two series 
- Sx - Standard deviation of series X
- Sy - Standard deviation of series Y
