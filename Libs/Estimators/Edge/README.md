# Edge Ratio 

Edge or E-Ratio is a ratio between MFE and MAE to identify if trades have bias towards profit. 
Can be calculated before the trade to predict the best time to open position. 
In this case, it needs to be normalized for volatility, e.g. by dividing by ATR value. 

$$ Edge = \frac{MFE}{MAE} $$ 

- MFE - Maximum favorable excursion 
- MAE - Maximum adverse excursion
