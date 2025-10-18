import { ApolloServer } from '@apollo/server';
import { startStandaloneServer } from '@apollo/server/standalone';
import { buildSubgraphSchema } from '@apollo/subgraph';
import gql from 'graphql-tag';

const typeDefs = gql`
  extend type Hotel @key(fields: "id") {
    id: ID! @external
  }

  type Booking @key(fields: "id") {
    id: ID!
    userId: String!
    hotelId: String!
    hotel: Hotel
    promoCode: String
    discountPercent: Int
  }

  type Query {
    bookingsByUser(userId: String!): [Booking]
  }

`;

const resolvers = {
  Booking: {
    __resolveReference: async ({ id }, context) => {
      if (context.req?.headers['userid'] !== 'user1') {
        throw new Error('Unauthorized');
      }
      return {
        id: id,
        userId: 'user1',
        hotelId: 'hotel1',
        discountPercent: 10,
        promoCode: 'PROMO1',
      };
    },
    hotel: (booking) => {
      return { __typename: 'Hotel', id: booking.hotelId };
    },
  },
  Query: {
    bookingsByUser: async (_, { userId }, { req }) => {
		// TODO: Реальный вызов к grpc booking-сервису или заглушка + ACL   

      if (req?.headers['userid'] !== 'user1') {
        throw new Error('Unauthorized');
      }

      return [
          {
              id: 'booking1',
              userId: userId,
              hotelId: 'hotel1',
              discountPercent: 10,
              promoCode: 'PROMO1',
          },
      ];
    },
  }
  
};

const server = new ApolloServer({
  schema: buildSubgraphSchema([{ typeDefs, resolvers }]),
});

startStandaloneServer(server, {
  listen: { port: 4001 },
  context: async ({ req }) => ({ req }),
}).then(() => {
  console.log('✅ Booking subgraph ready at http://localhost:4001/');
});
