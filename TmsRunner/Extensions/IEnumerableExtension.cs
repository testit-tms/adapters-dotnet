namespace TmsRunner.Extensions
{
    public static class IEnumerableExtension
    {
        public static IEnumerable<TSource> Flatten<TSource>(
            this IEnumerable<TSource> source,
            Func<TSource, IEnumerable<TSource>> getChildrenFunction)
        {
            return source.Aggregate(source,
                (current, element) =>
                    current.Concat(getChildrenFunction(element)
                        .Flatten(getChildrenFunction)
                    )
            );
        }
    }
}