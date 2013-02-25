# An idiomatic F# implementation of Domain-Driven Design

Blog post: [Domain-Driven Design With F# and EventStore](http://gorodinski.com/blog/2013/02/17/domain-driven-design-with-fsharp-and-eventstore/)

Based on [SimpleCQRS](https://github.com/gregoryyoung/m-r) by [Greg Young](http://goodenoughsoftware.net/)

Uses [EventStore](http://geteventstore.com/) for event persistance.

To run the Xunit integration tests:

* Run EventStore.SingleNode.exe --run-projections
* Enable the $by_category projection.
* Create new projections *FlatReadModelProjection.js* and *OverviewReadModelProjection.js* with mode=**Continuous** and **Emit** enabled.