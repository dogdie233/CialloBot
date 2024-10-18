using System.Linq.Expressions;
using System.Reflection;

using Lagrange.Core.Event;
using Lagrange.Core.Message;

namespace TestPlugin;

public static class BakaLogDestFinder
{
    private delegate MessageBuilder? MessageBuilderFactoryFunc(EventBase @event);

    private static readonly Dictionary<Type, MessageBuilderFactoryFunc> _cache = new();

    public static MessageBuilder? TryMakeDest(EventBase @event)
    {
        if (_cache.TryGetValue(@event.GetType(), out var func))
            return func(@event);

        func = GenerateMessageBuilderConstructorFunc(@event.GetType());
        _cache[@event.GetType()] = func;

        return func(@event);
    }

    private static MessageBuilderFactoryFunc GenerateMessageBuilderConstructorFunc(Type eventType)
    {
        var chainProperty = eventType
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .FirstOrDefault(property => property.PropertyType == typeof(MessageChain));
        if (chainProperty == null)
            return NullMessageBuilderFactory;

        var param = Expression.Parameter(typeof(EventBase), "@event");
        var nullExpr = Expression.Convert(Expression.Constant(null), typeof(MessageBuilder)); // Expression.Return(Expression.Label(), Expression.Constant(null));
        var castExpr = Expression.Convert(param, eventType);
        var chainGetter = Expression.Property(castExpr, chainProperty);
        var groupUinNullableGetter = Expression.Property(chainGetter, "GroupUin");
        var groupConstructor = typeof(MessageBuilder).GetMethod("Group", BindingFlags.Public | BindingFlags.Static, [typeof(uint)])!;
        var friendConstructor = typeof(MessageBuilder).GetMethod("Friend", BindingFlags.Public | BindingFlags.Static, [typeof(uint)])!;

        var chainNullCheck = Expression.Equal(chainGetter, Expression.Constant(null));
        var groupHasValueCheck = Expression.IsTrue(Expression.Property(groupUinNullableGetter, "HasValue"));
        var constructGroupMessageBuilder = Expression.Call(groupConstructor, Expression.Property(groupUinNullableGetter, "Value"));
        var constructFriendMessageBuilder = Expression.Call(friendConstructor, Expression.Property(chainGetter, "FriendUin"));

        var constructBuilderExpr = Expression.Condition(groupHasValueCheck, constructGroupMessageBuilder, constructFriendMessageBuilder);
        var body = Expression.Condition(chainNullCheck, nullExpr, constructBuilderExpr);
        var lambda = Expression.Lambda<MessageBuilderFactoryFunc>(body, param);

        return lambda.Compile();
    }

    private static MessageBuilder? NullMessageBuilderFactory(EventBase @event)
        => null;
}