import { ApolloServer } from '@apollo/server';
import { startStandaloneServer } from '@apollo/server/standalone';
import { ApolloGateway, RemoteGraphQLDataSource } from '@apollo/gateway';


const gateway = new ApolloGateway({
  serviceList: [
    { name: 'booking', url: 'http://booking-subgraph:4001' },
    { name: 'hotel', url: 'http://hotel-subgraph:4002' }
  ],
  debug: true,
  logger: console,
  buildService: ({ name, url }) => {
    return new RemoteGraphQLDataSource({
        url,
        willSendRequest({ request, context }) {
          const userId = request.variables?.userId;
          
          if (userId) {
            request.http.headers.set('userid', userId);
          } 
        }
    });
  }
});

const server = new ApolloServer({ 
  gateway, 
  subscriptions: false,
  csrfPrevention: false,
  includeStacktraceInErrorResponses: true,
  logger: console,
  plugins: [
    {
      async requestDidStart() {
        return {
          async parsingDidStart() {
            console.log('[Gateway] Parsing request');
          },
          async validationDidStart() {
            console.log('[Gateway] Validating request');
          },
          async executionDidStart() {
            console.log('[Gateway] Executing request');
          },
          async didEncounterErrors(requestContext) {
            console.error('[Gateway] Errors:', requestContext.errors);
          },
          async willSendResponse(requestContext) {
            console.log('[Gateway] Sending response');
          }
        };
      }
    }
  ]
});

startStandaloneServer(server, {
  listen: { port: 4000 },
  context: async ({ req }) => ({ req }), // headers пробрасываются
}).then(({ url }) => {
  console.log(`🚀 Gateway ready at ${url}`);
});
