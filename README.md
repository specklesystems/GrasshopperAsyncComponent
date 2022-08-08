# GrasshopperAsyncComponent

[![Twitter Follow](https://img.shields.io/twitter/follow/SpeckleSystems?style=social)](https://twitter.com/SpeckleSystems) [![Discourse users](https://img.shields.io/discourse/users?server=https%3A%2F%2Fdiscourse.speckle.works&style=flat-square)](https://discourse.speckle.works)
[![Slack Invite](https://img.shields.io/badge/-slack-grey?style=flat-square&logo=slack)](https://speckle-works.slack.com/join/shared_invite/enQtNjY5Mzk2NTYxNTA4LTU4MWI5ZjdhMjFmMTIxZDIzOTAzMzRmMTZhY2QxMmM1ZjVmNzJmZGMzMDVlZmJjYWQxYWU0MWJkYmY3N2JjNGI) [![website](https://img.shields.io/badge/www-speckle.systems-royalblue?style=flat-square)](https://speckle.systems)

## Less Janky Grasshopper Components

See the [companion blog post](https://speckle.systems/blog/async-gh/) about the rationale behind this approach. This repo demonstrates how to create an eager and responsive async component that does not block the Grasshopper UI thread while doing heavy work in the background, reports on progress and - theoretically - makes your life easier. 

We're not so sure about the last part! We've put this repo out in the hope that others will find something useful inside - even just inspiration for the approach.

![uselesscycles](https://user-images.githubusercontent.com/7696515/95028615-38583580-0699-11eb-8192-06c9cb4c3185.gif)

Looks nice, doesn't it? Notice that the solution runs "eagerly" - every time the input changes, the the computation restarts and cancels any previous tasks that are still running. Once everything is done calculating, the results are set. **And the best parts:** 

- **Grasshopper and Rhino are still responsive!**
- **There's progress reporting!** (personally I hate waiting for Gh to unfreeze...).
- **Thread safe**: 99% of the times this won't explode in your face. It still might though!

### Approach

Provides an abstract `GH_AsyncComponent` which you can inherit from to scaffold your own async component. There's more info in the [blogpost](https://speckle.systems/blog/async-gh/) on how to go about it.

> #### Checkout the sample implementation! 
> - [Prime number calculator](https://github.com/specklesystems/GrasshopperAsyncComponent/blob/a53cef31a8750a18d06fad0f41b2dc452fdc253b/GrasshopperAsyncComponentDemo/SampleImplementations/Sample_PrimeCalculatorAsyncComponent.cs#L11-L53) Calculates the n'th prime. Can actually spin your computer's fans quite a bit for numbers > 100.000!
> - [Usless spinner](https://github.com/specklesystems/GrasshopperAsyncComponent/blob/2f2be53bffd2402337ba40d65bb5b619d1161b3e/GrasshopperAsyncComponentDemo/SampleImplementations/Sample_UslessCyclesComponent.cs#L13-L91) does no meaningfull CPU work, just keeps a thread busy with SpinWait().

### Current limitations

~~Main current limitation is around data matching.~~ Solved! See [this PR](https://github.com/specklesystems/GrasshopperAsyncComponent/pull/4). Components inheriting from the GH_AsyncComponent class can now nicely handle multiple runs and any kind of data matching:

![oneproblemsolved](https://user-images.githubusercontent.com/7696515/95373253-a89ecb00-08d4-11eb-9bd9-9501caa0938e.gif)

~~Flash of null data~~ Solved! These Async Components now only expire their downstream dependants when they're done with their tasks. 

![lookmanoflash-2](https://user-images.githubusercontent.com/7696515/95596003-bbd0a880-0a44-11eb-90df-044b18dcc019.gif)

Other limitations: 

- This approach is most efficient if you can batch together as many iterations as possible. Ideally you'd work with trees straight away. 

- Task cancellation is up to the developer: this approach won't be too well suited for components calling code from other libraries that you don't, or can't, manage. 

### FAQ

Q: Does this component use all my cores? A: OH YES. It goes WROOOM.

![image](https://user-images.githubusercontent.com/7696515/95597125-29310900-0a46-11eb-99ce-663b34506a7a.png)

Q: Can I enable cancellation of a longer running task? 

A: Yes, now you can! In your component, just add a right click menu action like so:

```cs

    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalMenuItems(menu);
      Menu_AppendItem(menu, "Cancel", (s, e) =>
      {
        RequestCancellation();
      });
    }

```



### Debugging

Quite easy:
- Clone this repository and open up the solution in Visual Studio. 
- Once you've built it, add the `bin` folder to the Grasshopper Developer Settings (type `GrasshopperDeveloperSettings` in the Rhino command line) and open up Grasshopper. 
- You should see a new component popping up under "Samples > Async" in the ribbon. 
- A simple 

## Contributing

Please make sure you read the [contribution guidelines](.github/CONTRIBUTING.md) and [Code of Conduct](.github/CODE_OF_CONDUCT.md) for an overview of the best practices we try to follow.

## Community

The Speckle Community hangs out on [the forum](https://discourse.speckle.works), do join and introduce yourself & feel free to ask us questions!

## Security

For any security vulnerabilities or concerns, please contact us directly at security[at]speckle.systems.

## License

Unless otherwise described, the code in this repository is licensed under the Apache-2.0 License. Please note that some modules, extensions or code herein might be otherwise licensed. This is indicated either in the root of the containing folder under a different license file, or in the respective file's header. If you have any questions, don't hesitate to get in touch with us via [email](mailto:hello@speckle.systems).
