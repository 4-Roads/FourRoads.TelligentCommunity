using ManifestBuilder;

if (args.Length == 0 || !Builder.ValidPath(args[0]))
{
    Console.WriteLine("provide path to the folder that contains lib and code folders");
    return;
}

Builder.Build(args[0]);
