# Endpoints

Assembly

1. endpoint.Mapxxx();
每个Map里面都是一个独立的Application
由ApplicationBuilder创建
var appBuilder = new xxxAppliationBuilder(endpoints.CreateApplicatoinBuilder())
var app = appBuilder.Build();

var endpointsFactory = CreateEndpointsFactory();
endpointsFactory.SetProxyPipeline(app);
endpointsFactory中会有一个MatchPolicy, 用来匹配可以处理的请求


反向代理是一套路由(Endpoints) Matcher系统
匹配到的处理, 没匹配到继续往下找
最终没有找到，兜底的直接返回404


Endpoint Match System
系统注册所有的MatcherPolicy
Match的时候Foreach判断所有Policy看是否可以处理


## MapAppleAppEndpoints

IEndpointRouteBuilder.MapEndpoints实际是往当前的Endpoints.DataSource中添加一个新的DataSource

DataSource.RequestProcessPipeline 是这个Endpoints独有的请求处理管道，这个猜测是总管道执行完成后，针对具体Endpoints的子管道

DataSource要返回能处理的Endpoint, 
不同的Endpoint有不同的RoutePattern
RoutePattern只是匹配请求格式, 但是并不是绝对最终请求是否有这个Endpoint处理的关键步骤

这个决定的关键步骤是MatcherPolicy是否能成功匹配,他会检查的更多

### asp.net core mvc的路由匹配
1. 先会按照RoutePattern进行地址匹配
1. 对于匹配到的地址，再通过Matcher来进行代码逻辑匹配, 比如检查各种constraint

match的时候会给出一个candidates, 

匹配到多个candidate, 看起来会执行第一个





