# Domain glossary

## Class item stock evolution

Class item stock evolution is the running balance of signed `StockTransaction.QuantityChange`
values for one item across every location in a school class. Its timeline begins with the first
transaction for that class and item, with all transaction dates grouped by UTC calendar day.
Transfer-out and transfer-in entries offset each other in the class-wide total.
