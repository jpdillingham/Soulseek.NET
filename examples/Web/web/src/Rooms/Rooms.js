import React, { Component, createRef } from 'react';
import api from '../api';
import { activeRoomKey } from '../config';

import { Segment } from 'semantic-ui-react';

import RoomMenu from './RoomMenu';

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
        messages: window.setInterval(this.fetchActiveRoom, 500),
        users: window.setInterval(() => this.fetchActiveRoom({ includeUsers: true }), 5000)
      }
    }, () => this.fetchActiveRoom({ includeUsers: true }));
  }

  fetchJoinedRooms = async () => {
    const rooms = (await api.get('/rooms/joined')).data;
    this.setState({
      rooms
    });
  }

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
  }

  selectRoom = async (roomName) => {
    this.setState({ 
      active: roomName, 
      room: initialState.room 
    }, () => {
      sessionStorage.setItem(activeRoomKey, roomName);
      this.fetchActiveRoom({ includeUsers: true });
    });
  }

  joinRoom = async (roomName) => {
    await api.post(`/rooms/joined/${roomName}`);
  }

  leaveRoom = async (roomName) => {
    await api.delete(`/rooms/joined/${roomName}`);
  }

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
        <Segment>
          <pre>
            {JSON.stringify(room, null, 2)}
          </pre>
        </Segment>
      </div>
    )
  }
}

export default Rooms;