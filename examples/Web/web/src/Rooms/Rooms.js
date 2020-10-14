import React, { Component, createRef } from 'react';
import api from '../api';
import { activeRoomKey } from '../config';

import { Segment, Card, Icon, Input, Ref, List } from 'semantic-ui-react';

import RoomMenu from './RoomMenu';
import RoomUserList from './RoomUserList';

const initialState = {
  active: '',
  rooms: [],
  room: {
    messages: [],
    users: []
  },
  intervals: {
    rooms: undefined,
    messages: undefined,
    users: undefined
  }
}

class Rooms extends Component {
  state = initialState;
  messageRef = undefined;
  listRef = createRef();

  componentDidMount = () => {
    this.fetchJoinedRooms();
    this.setState({ 
      active: sessionStorage.getItem(activeRoomKey) || '',
      intervals: {
        rooms: window.setInterval(this.fetchJoinedRooms, 500),
        messages: window.setInterval(this.fetchActiveRoom, 1000),
        users: window.setInterval(() => this.fetchActiveRoom({ includeUsers: true }), 5000)
      }
    }, () => this.fetchActiveRoom({ includeUsers: true }));
  };

  fetchJoinedRooms = async () => {
    const rooms = (await api.get('/rooms/joined')).data;
    this.setState({
      rooms
    });
  };

  fetchActiveRoom = async ({ includeUsers = false } = {}) => {
    const { active, room } = this.state;

    if (active.length === 0) return;

    const messages = (await api.get(`/rooms/joined/${active}/messages`)).data;

    let { users } = room;

    if (includeUsers) {
      users = (await api.get(`/rooms/joined/${active}/users`)).data;
    }

    this.setState({
      room: {
        users,
        messages
      }
    });
  };

  selectRoom = async (roomName) => {
    this.setState({ 
      active: roomName, 
      room: initialState.room 
    }, async () => {
      sessionStorage.setItem(activeRoomKey, roomName);
      await this.fetchActiveRoom({ includeUsers: true });
      this.listRef.current.lastChild.scrollIntoView({ behavior: 'smooth' });
    });
  };

  joinRoom = async (roomName) => {
    await api.post(`/rooms/joined/${roomName}`);
  };

  leaveRoom = async (roomName) => {
    await api.delete(`/rooms/joined/${roomName}`);
    this.setState({ active: initialState.active }, () => {
      sessionStorage.removeItem(activeRoomKey);
    });
  };

  validInput = () => (this.state.active || '').length > 0 && ((this.messageRef && this.messageRef.current && this.messageRef.current.value) || '').length > 0;
  
  focusInput = () => {
    this.messageRef.current.focus();
  };

  formatTimestamp = (timestamp) => {
    const date = new Date(timestamp);
    const dtfUS = new Intl.DateTimeFormat('en', { 
        month: 'numeric', 
        day: 'numeric',
        hour: 'numeric',
        minute: '2-digit'
    });

    return dtfUS.format(date);
  };

  sendMessage = async () => {
    const { active } = this.state;
    const message = this.messageRef.current.value;

    if (!this.validInput()) {
      return;
    }

    await api.post(`/rooms/joined/${active}/messages`, JSON.stringify(message));
    this.messageRef.current.value = '';
  };

  render = () => {
    const { rooms, active, room } = this.state;

    return (
      <div className='rooms'>
        <Segment raised>
          <RoomMenu
            rooms={rooms}
            active={active}
            onRoomChange={(name) => this.selectRoom(name)}
            joinRoom={this.joinRoom}
          />
        </Segment>
        {active && <Card className='room-active-card' raised>
          <Card.Content onClick={() => this.focusInput()}>
            <Card.Header>
              <Icon name='circle' color='green'/>
              {active}
              <Icon 
                  className='close-button' 
                  name='close' 
                  color='red' 
                  link
                  onClick={() => this.leaveRoom(active)}
              />
            </Card.Header>
            <div className='room'>
              <Segment.Group>
                <Segment className='room-history'>
                  <Ref innerRef={this.listRef}>
                    <List>
                      {room.messages.map((message, index) =>
                        <List.Content
                          key={index}
                          className={`room-message ${!!message.self ? 'room-message-self' : ''}`}
                        >
                          <span className='room-message-time'>{this.formatTimestamp(message.timestamp)}</span>
                          <span className='room-message-name'>{message.username}: </span>
                          <span className='room-message-message'>{message.message}</span>
                        </List.Content>
                      )}
                      <List.Content id='room-history-scroll-anchor'/>
                    </List>
                  </Ref>
                </Segment>
                <Segment className='room-input'>
                  <Input
                    fluid
                    transparent
                    input={<input id='room-message-input' type="text" data-lpignore="true"></input>}
                    ref={input => this.messageRef = input && input.inputRef}
                    action={{
                        icon: <Icon name='send' color='green'/>,
                        className: 'room-message-button', onClick: this.sendMessage,
                        disabled: !this.validInput()
                    }}
                    onKeyUp={(e) => e.key === 'Enter' ? this.sendMessage() : ''}
                  />
                </Segment>
              </Segment.Group>
              <Segment className='room-users'>
                <RoomUserList users={room.users}/>
              </Segment>
            </div>
          </Card.Content>
        </Card>}
      </div>
    )
  };
};

export default Rooms;