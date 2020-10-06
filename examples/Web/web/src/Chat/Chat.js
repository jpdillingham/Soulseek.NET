import React, { Component, createRef } from 'react';
import api from '../api';
import './Chat.css';
import {
    Segment,
    List, Input, Card, Icon, Ref
} from 'semantic-ui-react';
import ConversationMenu from './ConversationMenu';

const initialState = {
    active: '',
    conversations: {},
    interval: undefined
};

class Chat extends Component {
    state = initialState;
    messageRef = undefined;
    listRef = createRef();

    componentDidMount = () => {
        this.fetchConversations();
        this.setState({ interval: window.setInterval(this.fetchConversations, 500) });
    }

    componentWillUnmount = () => {
        clearInterval(this.state.interval);
        this.setState({ interval: undefined });
    }

    fetchConversations = async () => {
        const { active } = this.state;
        let conversations = (await api.get('/conversations')).data;

        const unAckedActiveMessages = (conversations[active] || [])
            .filter(message => !message.acknowledged);

        if (unAckedActiveMessages.length > 0) {
            await this.acknowledgeMessages(active, { force: true });
            conversations = {
                ...conversations, 
                [active]: conversations[active].map(message => ({...message, acknowledged: true }))
            };
        };

        this.setState({ conversations }, () => {
            this.acknowledgeMessages(this.state.active);
        });
    }

    acknowledgeMessages = async (username, { force = false } = {}) => {
        if (!username) return;

        const unAckedMessages = (this.state.conversations[username] || [])
            .filter(message => !message.acknowledged);

        if (!force && unAckedMessages.length === 0) return;

        await api.put(`/conversations/${username}`);
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

    selectConversation = (username) => {
        this.setState({ active: username }, () => {
            this.acknowledgeMessages(this.state.active);
            this.listRef.current.lastChild.scrollIntoView({ behavior: 'smooth' });
        });
    }

    render = () => {
        const { conversations, active } = this.state;
        const messages = conversations[active] || [];

        return (
            <div className='chat-container'>
                <Card className='chat-menu' fluid raised>
                    <ConversationMenu
                        conversations={conversations}
                        active={active}
                        onConversationChange={(name) => this.selectConversation(name)}
                    />
                </Card>
                {active && <Card className='chat-active' fluid raised>
                    <Card.Content>
                        <Card.Header><Icon name='circle' color='green'/>{active}</Card.Header>
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
                </Card>}
            </div>
        )
    }
}

export default Chat;