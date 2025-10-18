import { ApolloServer } from '@apollo/server';
import { startStandaloneServer } from '@apollo/server/standalone';
import { buildSubgraphSchema } from '@apollo/subgraph';
import gql from 'graphql-tag';

const typeDefs = gql`
  type Hotel @key(fields: "id") {
    id: ID!
    name: String
    city: String
    stars: Int
  }

  type Query {
    hotelsByIds(ids: [ID!]!): [Hotel]
  }
`;

const resolvers = {
  Hotel: {
    __resolveReference: async ({ id }, context) => {
      
      // TODO: Заглушка - replace with actual data source
      return {
        id: id,
        name: `Hotel ${id}`,
        city: 'City 1',
        stars: 5
      };
    },
  },
  Query: {
    hotelsByIds: async (_, { ids }, { req }) => {
      // TODO: Заглушка или REST-запрос
      
      return ids.map(id => ({
        id: id,
        name: `Hotel ${id}`,
        city: 'City 1',
        stars: 5
      }));
    },
  },
};

const server = new ApolloServer({
  schema: buildSubgraphSchema([{ typeDefs, resolvers }]),
});

startStandaloneServer(server, {
  listen: { port: 4002 },
  context: async ({ req }) => ({ req }),
}).then(() => {
  console.log('✅ Hotel subgraph ready at http://localhost:4002/');
});
