Bootstrapper
============

Adapted from Yeppp library. Used for embedding native and managed dll 
as resources and loading them automatically. Bootstrapper itself 
contains MSVCRT9 as an example. (Some native libraries compiled for Windows
with CMAKE/VS require MSVCRT and fail on non-dev machines.)

It could be used by copying the files into a project. Or it could be referenced. That is, an embedded 
managed dll could contain other embedded dlls. The only requirement is to initialize 
the Bootstrapper static constructor by referencing Bootsrapper.Instance anywhere at 
the beginning of a program or in a static constructor of a main class in a library.

It does the job well for native libraries, but all other functionality it in alpha.

Licence
--------
New BSD license (AKA 3-clause BSD)