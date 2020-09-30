import React, { Component } from 'react';
import api from '../api';

import {
    Segment,
    List, Grid
} from 'semantic-ui-react';

const initialState = {
    conversations: {
        user1: [],
        user2: [],
        user3: []
    }
};

class Chat extends Component {
    state = initialState;

    render = () => {
        const { conversations } = this.state;

        return (
            <div className='chat-container'>
                <Grid stackable columns={2} className='chat-grid'>
                    <Grid.Row>
                        <Grid.Column width={4}>
                            <Segment className='chat-names-segment' raised>
                                <List>
                                    {Object.keys(conversations).map(name => <List.Item>{name}</List.Item>)}
                                </List>
                            </Segment>
                        </Grid.Column>
                        <Grid.Column width={12}>
                            <Segment className='chat-active-segment' raised>
                                <p>active chats go here</p>
                            </Segment>
                        </Grid.Column>
                    </Grid.Row>
                </Grid>
            </div>
        )
    }
}

export default Chat;