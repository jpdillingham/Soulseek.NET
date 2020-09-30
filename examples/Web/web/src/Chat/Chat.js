import React, { Component } from 'react';
import api from '../api';

const initialState = {
    conversations: {}
};

class Chat extends Component {
    state = initialState;

    render = () => {
        return (
            <div>Chats</div>
        )
    }
}

export default Chat;