import React, { Component } from 'react';
import api from '../api';

import {
    Segment,
    List, Grid, Input, Card, Icon
} from 'semantic-ui-react';

const initialState = {
    active: 'praetor-',
    conversations: {
        user1: [],
        user2: [],
        user3: []
    },
    interval: undefined
};

class Chat extends Component {
    state = initialState;
    messageRef = undefined;

    componentDidMount = () => {
        this.fetchConversations();
        this.setState({ interval: window.setInterval(this.fetchConversations, 500) });
    }

    fetchConversations = async () => {
        const conversations = (await api.get('/conversations')).data;
        this.setState({ conversations });
    }

    sendMessage = async () => {
        await api.post(`/conversations/${this.state.active}`, JSON.stringify(this.messageRef.current.value));
        this.messageRef.current.value = '';
    }

    formatTimestamp = (timestamp) => {
        const date = new Date(timestamp);
        const dtfUS = new Intl.DateTimeFormat('en', { 
            month: 'numeric', 
            day: 'numeric',
            hour: 'numeric',
            minute: '2-digit'
        });

        return dtfUS.format(date);
    }

    render = () => {
        const { conversations, active } = this.state;
        const messages = conversations[active] || [];

        return (
            <div className='chat-container'>
                <Grid stackable columns={2} className='chat-grid'>
                    <Grid.Row className='chat-grid-row'>
                        <Grid.Column className='chat-column-names' width={4}>
                            <Segment className='chat-names-segment' raised>
                                <List>
                                    {Object.keys(conversations).map((name, index) => <List.Item key={index}>{name}</List.Item>)}
                                </List>
                            </Segment>
                        </Grid.Column>
                        <Grid.Column className='chat-column-history' width={12}>
                            <Card className='chat-active-segment' fluid raised>
                                <Card.Content>
                                    <Card.Header><Icon name='chat'/>{active}</Card.Header>
                                    <Segment.Group>
                                        <Segment className='chat-history'>
                                            <List>
                                                {messages.map((message, index) => 
                                                    <List.Content 
                                                        key={index}
                                                        className={`chat-message ${message.username !== active ? 'chat-message-self' : ''}`}
                                                    >
                                                        <span className='chat-message-time'>{this.formatTimestamp(message.timestamp)}</span>
                                                        <span className='chat-message-name'>{message.username}: </span>
                                                        <span className='chat-message-message'>{message.message}</span>
                                                    </List.Content>
                                                )}
                                            </List>
                                        </Segment>
                                        <Segment className='chat-input'>
                                            <Input
                                                fluid
                                                transparent
                                                input={<input type="text" data-lpignore="true"></input>}
                                                ref={input => this.messageRef = input && input.inputRef}
                                                action={{ icon: 'send', className: 'chat-message-button', onClick: this.sendMessage }}
                                                onKeyUp={(e) => e.key === 'Enter' ? this.sendMessage() : ''}
                                            />
                                        </Segment>
                                    </Segment.Group>
                                </Card.Content>
                            </Card>
                        </Grid.Column>
                    </Grid.Row>
                </Grid>
            </div>
        )
    }
}

export default Chat;