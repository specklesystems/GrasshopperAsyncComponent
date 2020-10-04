# GrasshopperAsyncComponent

[![Twitter Follow](https://img.shields.io/twitter/follow/SpeckleSystems?style=social)](https://twitter.com/SpeckleSystems) [![Discourse users](https://img.shields.io/discourse/users?server=https%3A%2F%2Fdiscourse.speckle.works&style=flat-square)](https://discourse.speckle.works)
[![Slack Invite](https://img.shields.io/badge/-slack-grey?style=flat-square&logo=slack)](https://speckle-works.slack.com/join/shared_invite/enQtNjY5Mzk2NTYxNTA4LTU4MWI5ZjdhMjFmMTIxZDIzOTAzMzRmMTZhY2QxMmM1ZjVmNzJmZGMzMDVlZmJjYWQxYWU0MWJkYmY3N2JjNGI) [![website](https://img.shields.io/badge/www-speckle.systems-royalblue?style=flat-square)](https://speckle.systems)

## Less Janky Grasshopper Components

See the companion blog post about the rationale behind this approach. This repo demonstrates how to create an eager and responsive async component that does not block the Grasshopper UI thread while doing heavy work in the background, reports on progress and - theoretically - makes your life easier. 

We're not so sure about the last part! We've put this repo out in the hope that others will find something useful inside - even just inspiration for the approach.

INSERT GIF HERE



### Approach

Provides an abstract `GH_AsyncComponent` which you can inherit from to scaffold your own async component. There's more info in the blogpost on how to go about it.

Even better, there's a sample component that shows how an implementation could look like! TODO: link to code!

### Current limitations

Main current limitation is around data matching. If the component needs to run more than once - e.g., in the case of `Item` input parameters receiving a `List` of objects, it gets stuck. Currently, the safest bet - albeit not the easiest one - is to make sure your input parameters are set to `Tree` and you can do your own data matching inside. Not ideal, I know. I'm sure this can be improved, but it's late on a Sunday evening. 

### Debugging

Quite easy:
- Clone this repository and open up the solution in Visual Studio. 
- Once you've built it, add the `bin` folder to the Grasshopper Developer Settings (type `GrasshopperDeveloperSettings` in the Rhino command line) and open up Grasshopper. 
- You should see a new component popping up under "Samples > Async" in the ribbon. 
- A simple 

## Contributing

Before embarking on submitting a patch, please make sure you read:

- [Contribution Guidelines](CONTRIBUTING.md), 
- [Code of Conduct](CODE_OF_CONDUCT.md)

## License

Unless otherwise described, the code in this repository is licensed under the Apache-2.0 license.