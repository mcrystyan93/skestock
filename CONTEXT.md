# Domain glossary

## Class item stock evolution

Class item stock evolution is the running balance of signed `StockTransaction.QuantityChange`
values for one item across every location in a school class. Its timeline begins with the first
transaction for that class and item, with all transaction dates grouped by UTC calendar day.
Transfer-out and transfer-in entries offset each other in the class-wide total.

## Goods receipt (Recepție)

A goods receipt is one order that the school has received: a delivery with one or more received
item lines. It is the point at which purchased stock enters a school class.

## Purchase (Achiziție)

A purchase is one received item line. Its quantity is the amount received, not the amount still
left in stock, and its value is that quantity multiplied by the unit price paid. The purchase date
is the date the goods were received.

## Order list (Listă de comandă)

An order list is a plan of what to buy. It is not a purchase and does not count towards purchase
statistics until its items are actually received.

## Purchase frequency (Frecvență achiziții)

Purchase frequency is how many separate times an item was bought. All of an item's lines on the
same goods receipt count as one time.

## Top purchases (Top achiziții)

Top purchases ranks items by total purchased quantity, total purchase value, or purchase
frequency. The ranking covers either one school class's whole history or the last 90 or 365
calendar days across all classes. Quantities of items with different units are ranked together and
shown with their unit.
