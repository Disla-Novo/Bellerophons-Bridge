# Contributing

Thanks for considering helping with Bellerophon's Bridge. The plugin is in alpha, and practical feedback is especially useful. You do not need to be an expert Grasshopper user to open an issue or suggest an improvement.

## Ways to help

- Suggest clearer Grasshopper workflows for producing ordered print paths.
- Share ideas for making the component easier to use, including input names, defaults, warnings, and save behavior.
- Report confusing behavior or bugs in the exporter.
- The [README](README.md) does its best to offer suggestions, but we aren't experts! If you know Grasshopper well, share a small example definition showing a useful approach to contours, curve division, data-tree organization, or path sampling. 
- Contribute code or documentation through a pull request.

**Is it a compiler issue?** 
Please open an issue at the [Dimidium Ecosystem](https://github.com/Disla-Novo/Dimidium_Bellerophon). Be sure to read the [Dimidium Ecosystem CONTRIBUTING.md](https://github.com/Disla-Novo/Dimidium_Bellerophon/blob/main/CONTRIBUTING.md) before posting so you know exactly what compiler-specific details to include and what the requirements are. Thank you!

Please keep the plugin's scope in mind: Bellerophon's Bridge exports points from paths created in Grasshopper and can convert slicer movement commands into Bellerophon source. The Bellerophon compiler handles conversion to printer-specific G-code; the plugin is not intended to replace a slicer or handle heavy computation.

## Bug reports and workflow suggestions

When reporting an issue, include as many of these details as you can:

- Rhino and Grasshopper versions, operating system, and plugin version or commit
- What geometry and Grasshopper components you used
- Which exporter was used and, for point input, how the point data is organized (for example, one branch per continuous toolpath)
- What you expected and what happened
- A small `.gh` example and the relevant exported Bellerophon text, if you can share them
- Whether the result was checked in the Dimidium Ecosystem's Bellerophon compiler

Remove personal, confidential, or machine-specific information from example files before sharing. Do not include credentials or private network configuration.

## Pull requests

1. Open an issue first for larger changes so the approach can be discussed.
2. Keep changes focused, and update the README when user-visible behavior or setup changes.
3. Build the project before submitting and describe what changed, how you tested it, and any Rhino/Grasshopper versions involved.

Because this is alpha software, please avoid changing the Grasshopper component GUID unless a component identity change is intentional and its impact on existing definitions has been considered.

## License

Contributions to this project are intended to be distributed under the GNU General Public License v3.0. See [LICENSE](LICENSE).
