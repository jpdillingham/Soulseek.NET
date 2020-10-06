import React, { Component, createRef } from 'react';
import api from '../api';

import {
    Segment,
    List, Grid, Input, Card, Icon, Ref, Tab
} from 'semantic-ui-react';

const initialState = {
    active: '',
    conversations: {}
};

class Chat extends Component {
    state = initialState;
    messageRef = undefined;
    listRef = createRef();

    componentDidMount = () => {
        this.fetchConversations();
    }

    fetchConversations = async () => {
        const conversations = (await api.get('/conversations')).data;
        this.setState({ conversations });
    }

    sendMessage = async () => {
        await api.post(`/conversations/${this.state.active}`, JSON.stringify(this.messageRef.current.value));
        this.messageRef.current.value = '';
        this.listRef.current.lastChild.scrollIntoView({ behavior: 'smooth' });
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

    selectConversation = (username) => {
        this.setState({ active: username });
    }

    render = () => {
        const { conversations, active } = this.state;
        const messages = conversations[active] || [];

        return (
            <div className='chat-container'>
                <Card className='chat-active-segment' fluid raised>
                    <Card.Content>
                        <Tab 
                            onTabChange={(_, tabs) => this.selectConversation(tabs.panes[tabs.activeIndex].menuItem)} 
                            panes={Object.keys(conversations).map((name, index) => ({ menuItem: name }))}
                        />
                        {/* <Card.Header><Icon name='circle' color='green'/>{active}</Card.Header> */}
                        <Segment.Group>
                            <Segment className='chat-history'>
                                <Ref innerRef={this.listRef}>
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
                                        <List.Content id='chat-history-scroll-anchor'/>
                                    </List>
                                </Ref>
                            </Segment>
                            <Segment className='chat-input'>
                                <Input
                                    fluid
                                    transparent
                                    input={<input id='chat-message-input' type="text" data-lpignore="true"></input>}
                                    ref={input => this.messageRef = input && input.inputRef}
                                    action={{ icon: <Icon name='send' color='green'/>, className: 'chat-message-button', onClick: this.sendMessage }}
                                    onKeyUp={(e) => e.key === 'Enter' ? this.sendMessage() : ''}
                                />
                            </Segment>
                        </Segment.Group>
                    </Card.Content>
                </Card>
            </div>
        )
    }
}

export default Chat;