using System.Reflection;

namespace CialloBot.Test
{
    public class PluginServiceProviderTest
    {
        public interface IInterface1 { }
        public class Class1A : IInterface1 { }
        public class Class1B : IInterface1 { }
        public class PureClass { }

        [Fact]
        public void RegisterAndGetSameType_ReturnSame()
        {
            var provider = new PluginServiceProvider();
            var regType = typeof(IInterface1);
            var getType = regType;
            var impl = new Class1A();

            provider.RegisterService(regType, impl);
            var serviceGot = provider.GetService(getType);

            Assert.Same(impl, serviceGot);
        }

        [Fact]
        public void RegisterAndGetDerivedType_ReturnNull()
        {
            var provider = new PluginServiceProvider();
            var regType = typeof(IInterface1);
            var getType = typeof(Class1A);
            var impl = new Class1A();

            provider.RegisterService(regType, impl);
            var serviceGot = provider.GetService(getType);

            Assert.Null(serviceGot);
        }

        [Fact]
        public void RegisterAndGetDifferentType_ReturnNull()
        {
            var provider = new PluginServiceProvider();
            var regType = typeof(IInterface1);
            var getType = typeof(PureClass);
            var impl = new PureClass();

            provider.RegisterService(regType, impl);
            var serviceGot = provider.GetService(getType);

            Assert.Null(serviceGot);
        }

        [Fact]
        public void RegisterAndGetRequiredSameType_ReturnSame()
        {
            var provider = new PluginServiceProvider();
            var serviceType = typeof(IInterface1);
            var impl = new PureClass();

            provider.RegisterService(serviceType, impl);
            var serviceGot = provider.GetRequiredService(serviceType);

            Assert.Same(impl, serviceGot);
        }

        [Fact]
        public void RegisterTwoServiceWithSameServiceType_ReturnTailService()
        {
            var provider = new PluginServiceProvider();
            var regType = typeof(IInterface1);
            var getType = typeof(IInterface1);
            var implA = new Class1A();
            var implB = new Class1A();
            var privateContainer = GetContainer(provider);

            provider.RegisterService(regType, implA);
            provider.RegisterService(regType, implB);
            var serviceGot = provider.GetService(getType);

            Assert.Same(implB, serviceGot);
        }

        [Fact]
        public void GetRequiredNotRegisteredType_ThrowIOE()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                var provider = new PluginServiceProvider();
                var serviceType = typeof(IInterface1);
                var regType = typeof(PureClass);
                var impl = new PureClass();

                provider.RegisterService(regType, impl);
                var serviceGot = provider.GetRequiredService(serviceType);
            });
        }

        [Fact]
        public void GetRequiredNotRegisteredDerivedType_ThrowIOE()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                var provider = new PluginServiceProvider();
                var regType = typeof(IInterface1);
                var getType = typeof(PureClass);
                var impl = new PureClass();

                provider.RegisterService(regType, impl);
                var serviceGot = provider.GetRequiredService(getType);
            });
        }

        [Fact]
        public void UnregisterNotExistService_Ok()
        {
            var provider = new PluginServiceProvider();
            var impl = new PureClass();

            provider.UnregisterService(impl);
        }

        [Fact]
        public void AddAndRemoveService_Ok()
        {
            var provider = new PluginServiceProvider();
            var regType = typeof(IInterface1);
            var getType = typeof(Class1A);
            var impl = new Class1A();
            var privateContainer = GetContainer(provider);

            provider.RegisterService(regType, impl);
            provider.UnregisterService(impl);

            Assert.Empty(privateContainer);
        }

        [Fact]
        public void RegisterTwoServiceWithSameServiceTypeAndRemoveFirstAndGet_ReturnSecondService()
        {
            var provider = new PluginServiceProvider();
            var regType = typeof(IInterface1);
            var getType = regType;
            var implA = new Class1A();
            var implB = new Class1A();
            var privateContainer = GetContainer(provider);

            provider.RegisterService(regType, implA);
            provider.RegisterService(regType, implB);
            provider.UnregisterService(implA);
            var serviceGot = provider.GetService(getType);

            Assert.Same(implB, serviceGot);
        }

        private Dictionary<Type, List<PluginServiceProvider.ServiceDescription>> GetContainer(PluginServiceProvider provider)
            => (Dictionary<Type, List<PluginServiceProvider.ServiceDescription>>)provider.GetType().GetField("servicesContainer", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(provider)!;
    }
}